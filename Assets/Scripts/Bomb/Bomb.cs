using UnityEngine;

public class Bomb : MonoBehaviour
{
    public AudioClip bombSound;  // صوت القنبلة
    private AudioSource audioSource;

    private bool isHit = false;

    private void Start()
    {
        // إزالة القنبلة بعد 2 ثانية تلقائيًا
        Destroy(gameObject, 2f);

        // إعداد الصوت
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (bombSound != null)
        {
            audioSource.clip = bombSound;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHit) return;  // تأكد من عدم التكرار

        if (other.CompareTag("PlayerBlade"))
        {
            isHit = true;

            Debug.Log("💣 Bomb hit! Score will decrease!");

            // تقليل السكور
            ScoreManager.Instance.DecreaseScore(10);

            // تشغيل الصوت إذا وُجد
            if (bombSound != null)
            {
                audioSource.Play();
            }

            // إخفاء القنبلة مباشرة ولكن تأخير التدمير للسماح للصوت بالعمل
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;

            Destroy(gameObject, bombSound != null ? bombSound.length : 0.1f); // ننتظر انتهاء الصوت
        }
    }
}
