# Hammer Setup Guide

This guide will help you set up a sophisticated physics-based hammer that provides dynamic collision responses based on velocity - pushing, deforming, and crushing objects at different velocity thresholds.

## 🔨 Converting Your Existing Hammer

### Step 1: Prepare Your Hammer GameObject

1. **Select your hammer GameObject** in the scene
2. **Remove the Rigidbody component** (if it exists)
3. **Remove the MeshCollider component** (if it exists)
4. **Remove any old hammer scripts** (if they exist)

### Step 2: Add Required Components

1. **Add a Body component**:

   - Right-click on your hammer GameObject
   - Add Component → Scripts → Body Representation → Body
   - This will be the physics simulation component

2. **Add the HammerBody component**:
   - Add Component → Scripts → Hammer → HammerBody
   - This will automatically generate particles and constraints

### Step 3: Configure HammerBody Settings

#### Basic Hammer Generation:

```
Hammer Generation:
- Handle Length: 2.0 (length of the hammer handle)
- Handle Radius: 0.1 (thickness of the handle)
- Head Radius: 0.3 (size of the hammer head)
- Head Height: 0.4 (height of the hammer head)
- Handle Resolution: 6 (particles along handle)
- Head Resolution: 8 (particles in head)
- Total Mass: 3.0 (heavier hammer = more impact)
```

#### Physics Settings:

```
Hammer Physics:
- Surface Stiffness: 0.9 (how rigid the hammer surface is)
- Volume Stiffness: 0.7 (how rigid the hammer volume is)
- Connection Radius Multiplier: 2.0 (how connected particles are)
```

#### Mouse Control:

```
Mouse Control:
- Mouse Sensitivity: 8.0 (how responsive mouse control is)
- Max Distance From Camera: 15.0 (maximum distance from camera)
- Min Distance From Camera: 3.0 (minimum distance from camera)
- Ground Layer: 1 (layer for ground collision)
- Drag Damping: 0.95 (smoothness of movement)
```

#### Dynamic Collision Response:

```
Dynamic Collision Response:
- Push Velocity Threshold: 2.0 m/s (below this: push objects)
- Deform Velocity Threshold: 5.0 m/s (below this: deform objects)
- Crush Velocity Threshold: 8.0 m/s (above this: crush objects)
- Push Force: 3.0 (strength of push effect)
- Deform Force: 8.0 (strength of deform effect)
- Crush Force: 15.0 (strength of crush effect)
- Impact Radius: 0.5 (radius of impact effect)
```

### Step 4: Add to Simulation Manager

1. **Find your SimulationManager** in the scene
2. **Add your hammer's Body component** to the `bodies` list
3. **The hammer will now be part of the physics simulation**

## 🎮 How Dynamic Collision Response Works

### Velocity-Based Responses:

#### **Push Response** (< 2 m/s):

- **Effect**: Objects are gently pushed away
- **Use Case**: Light tapping, positioning objects
- **Visual**: Minimal particle effects
- **Audio**: Soft impact sound

#### **Deform Response** (2-8 m/s):

- **Effect**: Objects deform and lose shape temporarily
- **Use Case**: Medium impact, object manipulation
- **Visual**: Constraint stiffness reduction
- **Audio**: Medium impact sound
- **Recovery**: Objects gradually regain shape

#### **Crush Response** (> 8 m/s):

- **Effect**: Objects break apart and constraints are destroyed
- **Use Case**: High-speed impacts, destruction
- **Visual**: Particle explosions, body splitting
- **Audio**: Loud crush sound
- **Permanent**: Objects are permanently damaged

### Dynamic Thresholds:

- **All thresholds are configurable** in the inspector
- **No hardcoded values** - everything is adjustable
- **Real-time velocity tracking** for accurate responses
- **Impact cooldown** prevents multiple rapid impacts

## 🔧 Advanced Configuration

### For Realistic Hammer Physics:

```
Hammer Generation:
- Handle Length: 1.5-2.5 (realistic hammer proportions)
- Handle Radius: 0.08-0.12 (comfortable grip size)
- Head Radius: 0.25-0.35 (effective striking area)
- Total Mass: 2.0-4.0 (realistic hammer weight)
```

### For Better Performance:

```
Resolution Settings:
- Handle Resolution: 4-6 (fewer particles = better performance)
- Head Resolution: 6-8 (balance of detail and performance)
- Surface Stiffness: 0.8-0.95 (stiffer = more stable)
```

### For Different Game Styles:

#### **Destruction Game**:

```
Collision Response:
- Push Velocity Threshold: 1.0 (easier to push)
- Deform Velocity Threshold: 3.0 (easier to deform)
- Crush Velocity Threshold: 5.0 (easier to crush)
- Crush Force: 20.0 (more destructive)
```

#### **Precision Game**:

```
Collision Response:
- Push Velocity Threshold: 3.0 (harder to push)
- Deform Velocity Threshold: 7.0 (harder to deform)
- Crush Velocity Threshold: 12.0 (harder to crush)
- Push Force: 2.0 (gentler pushing)
```

#### **Physics Sandbox**:

```
Collision Response:
- Push Velocity Threshold: 1.5 (responsive pushing)
- Deform Velocity Threshold: 4.0 (good deformation)
- Crush Velocity Threshold: 8.0 (realistic crushing)
- All Forces: Balanced for experimentation
```

## 🎯 Tips and Tricks

### 1. **Hammer Design**:

- **Handle**: More particles = more flexible handle
- **Head**: Larger head = bigger impact area
- **Mass**: Heavier hammer = more momentum
- **Shape**: Tapered head for realistic appearance

### 2. **Mouse Control**:

- **Smooth movement** prevents physics instability
- **Distance constraints** keep hammer in playable area
- **Ground collision** prevents going through floor
- **Damping** makes movement feel natural

### 3. **Collision System**:

- **Velocity tracking** is frame-rate independent
- **Impact cooldown** prevents multiple rapid hits
- **Distance-based forces** create realistic impact patterns
- **Constraint breaking** allows for permanent damage

### 4. **Performance Optimization**:

- **Lower resolution** for better performance
- **Higher stiffness** for more stable physics
- **Smaller impact radius** for focused effects
- **Disable debug displays** in production

## 🚨 Troubleshooting

### Hammer Too Soft:

- Increase `Surface Stiffness` and `Volume Stiffness`
- Increase `Resolution` for more particles
- Increase `Total Mass` for more stability

### Hammer Too Rigid:

- Decrease `Surface Stiffness` and `Volume Stiffness`
- Decrease `Resolution` for fewer particles
- Decrease `Total Mass` for more flexibility

### Poor Collision Response:

- Check velocity thresholds are appropriate
- Increase impact forces
- Verify hammer is in SimulationManager bodies list
- Check impact radius is large enough

### Mouse Control Issues:

- Adjust `Mouse Sensitivity`
- Check `Ground Layer` is set correctly
- Adjust `Max/Min Distance From Camera`
- Increase `Drag Damping` for smoother movement

### Performance Issues:

- Reduce `Resolution` settings
- Reduce `Impact Radius`
- Disable debug displays
- Reduce particle count in other objects

## 🎮 Example Setups

### **Standard Game Hammer**:

```
Hammer Generation:
- Handle Length: 2.0
- Handle Radius: 0.1
- Head Radius: 0.3
- Head Height: 0.4
- Handle Resolution: 6
- Head Resolution: 8
- Total Mass: 3.0

Collision Response:
- Push Velocity Threshold: 2.0
- Deform Velocity Threshold: 5.0
- Crush Velocity Threshold: 8.0
- Push Force: 3.0
- Deform Force: 8.0
- Crush Force: 15.0
```

### **Heavy Sledgehammer**:

```
Hammer Generation:
- Handle Length: 2.5
- Handle Radius: 0.12
- Head Radius: 0.4
- Head Height: 0.5
- Total Mass: 5.0

Collision Response:
- Push Velocity Threshold: 1.5
- Deform Velocity Threshold: 3.0
- Crush Velocity Threshold: 6.0
- Crush Force: 25.0
```

### **Precision Hammer**:

```
Hammer Generation:
- Handle Length: 1.5
- Handle Radius: 0.08
- Head Radius: 0.2
- Head Height: 0.3
- Total Mass: 2.0

Collision Response:
- Push Velocity Threshold: 3.0
- Deform Velocity Threshold: 7.0
- Crush Velocity Threshold: 12.0
- Push Force: 2.0
```

## 🔮 Advanced Features

### Visual Effects:

- **Impact particle effects** at collision points
- **Velocity-based audio** (louder at higher speeds)
- **Debug displays** for velocity and impact radius
- **Customizable effect prefabs**

### Audio Integration:

- **Impact sounds** for different collision types
- **Crush sounds** for destructive impacts
- **Volume based on velocity** (louder = faster)
- **Automatic AudioSource setup**

### Debug Features:

- **Velocity display** in real-time
- **Impact radius visualization**
- **Collision response logging**
- **Performance monitoring**

---

_Your hammer is now a sophisticated physics body with dynamic collision responses that scale with velocity!_
