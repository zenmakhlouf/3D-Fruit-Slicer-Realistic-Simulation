using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// Improved Shape Matching Constraint with better numerical stability
/// </summary>
public class ShapeMatchingConstraint
{
    public float stiffness = 0.8f;
    private List<Particle> particles;
    private Vector3 restCenterOfMass;
    private List<Vector3> restPositions;
    private Matrix4x4 restCovariance;
    private Matrix4x4 invRestCovariance;
    private bool isValid = false;

    public ShapeMatchingConstraint(List<Particle> particles, float stiffness = 0.8f)
    {
        this.particles = new List<Particle>(particles);
        this.stiffness = Mathf.Clamp01(stiffness);
        this.restPositions = new List<Vector3>();

        if (particles.Count < 3)
        {
            Debug.LogWarning("Shape matching constraint needs at least 3 particles");
            return;
        }

        PrecomputeRestConfiguration();
    }

    private void PrecomputeRestConfiguration()
    {
        if (particles.Count == 0) return;

        // Calculate rest center of mass
        restCenterOfMass = Vector3.zero;
        float totalMass = 0f;

        foreach (var p in particles)
        {
            restCenterOfMass += p.position * p.mass;
            totalMass += p.mass;
        }

        if (totalMass > 0.001f)
        {
            restCenterOfMass /= totalMass;
        }

        // Calculate rest positions relative to center of mass
        restPositions.Clear();
        foreach (var p in particles)
        {
            restPositions.Add(p.position - restCenterOfMass);
        }

        // Build covariance matrix with better numerical stability
        restCovariance = Matrix4x4.zero;
        for (int i = 0; i < particles.Count; i++)
        {
            Vector3 q = restPositions[i];
            float mass = particles[i].mass;

            restCovariance.m00 += mass * q.x * q.x;
            restCovariance.m01 += mass * q.x * q.y;
            restCovariance.m02 += mass * q.x * q.z;
            restCovariance.m10 += mass * q.y * q.x;
            restCovariance.m11 += mass * q.y * q.y;
            restCovariance.m12 += mass * q.y * q.z;
            restCovariance.m20 += mass * q.z * q.x;
            restCovariance.m21 += mass * q.z * q.y;
            restCovariance.m22 += mass * q.z * q.z;
        }

        // Add regularization for numerical stability
        float regularization = 0.001f;
        restCovariance.m00 += regularization;
        restCovariance.m11 += regularization;
        restCovariance.m22 += regularization;
        restCovariance.m33 = 1f;

        // Calculate inverse with stability check
        float det = restCovariance.determinant;
        if (Mathf.Abs(det) > 0.001f)
        {
            invRestCovariance = restCovariance.inverse;
            isValid = true;
        }
        else
        {
            Debug.LogWarning("Shape matching constraint has singular rest configuration");
            isValid = false;
        }
    }

    public void Solve()
    {
        if (!isValid || particles.Count == 0) return;

        // Calculate current center of mass
        Vector3 currentCenterOfMass = Vector3.zero;
        float totalMass = 0f;

        foreach (var p in particles)
        {
            currentCenterOfMass += p.position * p.mass;
            totalMass += p.mass;
        }

        if (totalMass > 0.001f)
        {
            currentCenterOfMass /= totalMass;
        }

        // Build current covariance matrix
        Matrix4x4 currentCovariance = Matrix4x4.zero;
        for (int i = 0; i < particles.Count; i++)
        {
            Vector3 p_i = particles[i].position - currentCenterOfMass;
            Vector3 q_i = restPositions[i];
            float mass = particles[i].mass;

            currentCovariance.m00 += mass * p_i.x * q_i.x;
            currentCovariance.m01 += mass * p_i.x * q_i.y;
            currentCovariance.m02 += mass * p_i.x * q_i.z;
            currentCovariance.m10 += mass * p_i.y * q_i.x;
            currentCovariance.m11 += mass * p_i.y * q_i.y;
            currentCovariance.m12 += mass * p_i.y * q_i.z;
            currentCovariance.m20 += mass * p_i.z * q_i.x;
            currentCovariance.m21 += mass * p_i.z * q_i.y;
            currentCovariance.m22 += mass * p_i.z * q_i.z;
        }
        currentCovariance.m33 = 1f;

        // Calculate transformation matrix
        Matrix4x4 A = currentCovariance * invRestCovariance;

        // Extract rotation using improved polar decomposition
        Matrix4x4 R = ExtractRotation(A);

        // Apply corrections
        for (int i = 0; i < particles.Count; i++)
        {
            Vector3 goalPosition = MultiplyMatrix3x3Vector3(R, restPositions[i]) + currentCenterOfMass;
            Vector3 correction = (goalPosition - particles[i].position) * stiffness;
            particles[i].ApplyCorrection(correction);
        }
    }

    /// <summary>
    /// Improved polar decomposition with better numerical stability
    /// </summary>
    private Matrix4x4 ExtractRotation(Matrix4x4 A)
    {
        Matrix4x4 R = A;

        // Perform iterative polar decomposition
        for (int iter = 0; iter < 10; iter++)
        {
            Matrix4x4 Rt = TransposeMatrix3x3(R);
            float det = Determinant3x3(R);

            if (Mathf.Abs(det) < 0.001f) break;

            Matrix4x4 RtR_inv = InverseMatrix3x3(MultiplyMatrix3x3(Rt, R));
            Matrix4x4 R_next = MultiplyMatrix3x3(ScaleMatrix3x3(R, 0.5f),
                                               ScaleMatrix3x3(RtR_inv, 0.5f));

            // Check for convergence
            if (MatrixDistance3x3(R, R_next) < 0.001f) break;

            R = R_next;
        }

        return R;
    }

    // Helper matrix operations for 3x3 submatrices
    private Matrix4x4 MultiplyMatrix3x3(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = Matrix4x4.zero;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    result[i, j] += a[i, k] * b[k, j];
                }
            }
        }
        result.m33 = 1f;
        return result;
    }

    private Matrix4x4 TransposeMatrix3x3(Matrix4x4 m)
    {
        Matrix4x4 result = Matrix4x4.zero;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                result[i, j] = m[j, i];
            }
        }
        result.m33 = 1f;
        return result;
    }

    private Matrix4x4 ScaleMatrix3x3(Matrix4x4 m, float scale)
    {
        Matrix4x4 result = m;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                result[i, j] *= scale;
            }
        }
        return result;
    }

    private float Determinant3x3(Matrix4x4 m)
    {
        return m.m00 * (m.m11 * m.m22 - m.m12 * m.m21) -
               m.m01 * (m.m10 * m.m22 - m.m12 * m.m20) +
               m.m02 * (m.m10 * m.m21 - m.m11 * m.m20);
    }

    private Matrix4x4 InverseMatrix3x3(Matrix4x4 m)
    {
        float det = Determinant3x3(m);
        if (Mathf.Abs(det) < 0.001f) return Matrix4x4.identity;

        Matrix4x4 result = Matrix4x4.zero;
        float invDet = 1f / det;

        result.m00 = (m.m11 * m.m22 - m.m12 * m.m21) * invDet;
        result.m01 = (m.m02 * m.m21 - m.m01 * m.m22) * invDet;
        result.m02 = (m.m01 * m.m12 - m.m02 * m.m11) * invDet;
        result.m10 = (m.m12 * m.m20 - m.m10 * m.m22) * invDet;
        result.m11 = (m.m00 * m.m22 - m.m02 * m.m20) * invDet;
        result.m12 = (m.m02 * m.m10 - m.m00 * m.m12) * invDet;
        result.m20 = (m.m10 * m.m21 - m.m11 * m.m20) * invDet;
        result.m21 = (m.m01 * m.m20 - m.m00 * m.m21) * invDet;
        result.m22 = (m.m00 * m.m11 - m.m01 * m.m10) * invDet;
        result.m33 = 1f;

        return result;
    }

    private float MatrixDistance3x3(Matrix4x4 a, Matrix4x4 b)
    {
        float sum = 0f;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                float diff = a[i, j] - b[i, j];
                sum += diff * diff;
            }
        }
        return Mathf.Sqrt(sum);
    }

    private Vector3 MultiplyMatrix3x3Vector3(Matrix4x4 m, Vector3 v)
    {
        return new Vector3(
            m.m00 * v.x + m.m01 * v.y + m.m02 * v.z,
            m.m10 * v.x + m.m11 * v.y + m.m12 * v.z,
            m.m20 * v.x + m.m21 * v.y + m.m22 * v.z
        );
    }
}
