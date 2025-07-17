# Basket Setup Guide

This guide will help you convert your existing basket from a simple Rigidbody to a sophisticated physics body that interacts with fruits using the same particle system.

## 🧺 Converting Your Existing Basket

### Step 1: Prepare Your Basket GameObject

1. **Select your basket GameObject** in the scene
2. **Remove the Rigidbody component** (if it exists)
3. **Remove the MeshCollider component** (if it exists)
4. **Remove the old BasketCollector component** (if it exists)

### Step 2: Add Required Components

1. **Add a Body component**:

   - Right-click on your basket GameObject
   - Add Component → Scripts → Body Representation → Body
   - This will be the physics simulation component

2. **Add the BasketBody component**:

   - Add Component → Scripts → Basket → BasketBody
   - This will automatically generate particles and constraints

3. **Add the BasketCollector component** (optional):
   - Add Component → Scripts → Basket → BasketCollector
   - This provides additional collection features

### Step 3: Configure BasketBody Settings

#### Basic Settings:

```
Basket Generation:
- Basket Radius: 1.5 (adjust to match your basket size)
- Basket Height: 0.8 (adjust to match your basket height)
- Resolution: 8 (higher = more particles, better physics)
- Total Mass: 2.0 (heavier basket = more stable)
```

#### Physics Settings:

```
Basket Physics:
- Surface Stiffness: 0.8 (how rigid the basket surface is)
- Volume Stiffness: 0.6 (how rigid the basket volume is)
- Connection Radius Multiplier: 2.0 (how connected particles are)
```

#### Mouse Control:

```
Mouse Control:
- Mouse Sensitivity: 5.0 (how responsive mouse control is)
- Max Distance From Camera: 10.0 (maximum distance from camera)
- Min Distance From Camera: 2.0 (minimum distance from camera)
- Ground Layer: 1 (layer for ground collision)
```

#### Collection:

```
Collection:
- Collection Radius: 1.2 (how far fruits are attracted)
- Collection Force: 5.0 (how strongly fruits are pulled)
- Enable Collection: true (turn collection on/off)
```

### Step 4: Add to Simulation Manager

1. **Find your SimulationManager** in the scene
2. **Add your basket's Body component** to the `bodies` list
3. **The basket will now be part of the physics simulation**

## 🎮 How It Works

### Physics Simulation:

- The basket is now made of **particles and constraints**
- It behaves like a **soft, deformable body**
- It **collides with fruits** using the same physics system
- It **maintains its shape** through distance constraints

### Mouse Control:

- **Click and drag** to move the basket
- **Constrained to camera distance** (won't go too far/near)
- **Ground collision** prevents going through the floor
- **Smooth movement** with all particles moving together

### Fruit Collection:

- **Automatic attraction** when fruits get close
- **Physics-based collection** (fruits are pulled into basket)
- **Visual and audio effects** when collecting
- **Score tracking** integration

## 🔧 Advanced Configuration

### For Better Performance:

```
Resolution: 6-8 (good balance of physics and performance)
Surface Stiffness: 0.7-0.9 (stiffer = less deformation)
Volume Stiffness: 0.5-0.7 (stiffer = more rigid)
```

### For More Realistic Physics:

```
Resolution: 10-12 (more particles = better physics)
Surface Stiffness: 0.6-0.8 (softer = more realistic)
Volume Stiffness: 0.4-0.6 (softer = more flexible)
```

### For Better Collection:

```
Collection Radius: 1.0-1.5 (larger = easier collection)
Collection Force: 3.0-7.0 (stronger = faster collection)
```

## 🎯 Tips and Tricks

### 1. **Basket Shape**:

- The basket generates as a **cylinder with open top**
- **Bottom ring** has more particles for stability
- **Top ring** is slightly smaller for funnel effect
- **Internal particles** provide volume and stability

### 2. **Mouse Control**:

- **Click near the basket** to start dragging
- **Drag plane** follows the ground
- **Distance constraints** keep basket in playable area
- **Smooth movement** prevents physics instability

### 3. **Collection System**:

- **Automatic detection** of nearby fruits
- **Physics-based attraction** (not teleportation)
- **Visual feedback** with particle effects
- **Audio feedback** with collection sounds

### 4. **Performance**:

- **Higher resolution** = better physics but slower performance
- **More constraints** = more stable but more calculations
- **Collection radius** affects performance (larger = more checks)

## 🚨 Troubleshooting

### Basket Too Soft:

- Increase `Surface Stiffness` and `Volume Stiffness`
- Increase `Resolution` for more particles
- Increase `Total Mass` for more stability

### Basket Too Rigid:

- Decrease `Surface Stiffness` and `Volume Stiffness`
- Decrease `Resolution` for fewer particles
- Decrease `Total Mass` for more flexibility

### Poor Collection:

- Increase `Collection Radius`
- Increase `Collection Force`
- Check that `Enable Collection` is true

### Mouse Control Issues:

- Adjust `Mouse Sensitivity`
- Check `Ground Layer` is set correctly
- Adjust `Max/Min Distance From Camera`

### Performance Issues:

- Reduce `Resolution`
- Reduce `Collection Radius`
- Disable visual effects if needed

## 🎮 Example Setup

Here's a recommended setup for a typical fruit collection game:

```
BasketBody Settings:
- Basket Radius: 1.5
- Basket Height: 0.8
- Resolution: 8
- Total Mass: 2.0
- Surface Stiffness: 0.8
- Volume Stiffness: 0.6
- Collection Radius: 1.2
- Collection Force: 5.0
```

This setup provides:

- ✅ Good physics simulation
- ✅ Stable basket shape
- ✅ Responsive mouse control
- ✅ Effective fruit collection
- ✅ Good performance

---

_Your basket is now a sophisticated physics body that interacts naturally with fruits while maintaining smooth mouse control!_
