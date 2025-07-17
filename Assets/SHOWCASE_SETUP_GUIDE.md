# Showcase Setup Guide

This guide will help you set up 3 different showcase scenarios that demonstrate your physics simulation project in full, with a comprehensive menu system for parameter control.

## 🎮 **3 Showcase Scenarios**

### **1. Fruit Collection Paradise**

- **Focus**: Relaxing fruit collection with beautiful physics
- **Fruit Count**: 50 fruits
- **Tools**: Basket only
- **Physics**: Standard gravity, optimized for smooth gameplay
- **Best For**: Demonstrating smooth physics and collection mechanics

### **2. Destruction Derby**

- **Focus**: High-energy destruction with hammers and knives
- **Fruit Count**: 100 fruits
- **Tools**: Hammer and knife
- **Physics**: Higher gravity, optimized for destruction
- **Best For**: Showcasing dynamic collision responses and destruction

### **3. Physics Sandbox**

- **Focus**: Massive particle systems and performance
- **Fruit Count**: 500 fruits
- **Tools**: All tools available
- **Physics**: High gravity, performance mode
- **Best For**: Demonstrating scalability and performance optimizations

## 🛠️ **Setup Instructions**

### **Step 1: Create Scenes**

#### **Main Menu Scene**

1. **Create a new scene** called "MainMenu"
2. **Add UI elements**:
   - Canvas with UI elements
   - Main menu panel
   - Scenario selection panel
   - Parameters panel
   - Showcase controls panel

#### **Showcase Scene**

1. **Create a new scene** called "Showcase"
2. **Add core components**:
   - SimulationManager
   - ShowcaseSetup
   - PerformanceMonitor
   - Camera setup
   - Ground plane

### **Step 2: Setup Main Menu Scene**

#### **Add ShowcaseManager**

```csharp
// Add to a GameObject in MainMenu scene
var showcaseManager = gameObject.AddComponent<ShowcaseManager>();
```

#### **Add ShowcaseUI**

```csharp
// Add to a GameObject in MainMenu scene
var showcaseUI = gameObject.AddComponent<ShowcaseUI>();
```

#### **UI Layout Structure**

```
Canvas
├── MainMenuPanel
│   ├── Title Text
│   ├── Start Button
│   ├── Scenarios Button
│   ├── Quit Button
│   └── Version Text
├── ScenarioPanel
│   ├── Back Button
│   └── ScenarioButtonContainer (ScrollView)
├── ParametersPanel
│   ├── Fruit Parameters Section
│   ├── Physics Parameters Section
│   ├── Tools Parameters Section
│   ├── Hammer Parameters Section
│   ├── Start Scenario Button
│   ├── Back Button
│   └── Reset to Defaults Button
└── ShowcaseControlsPanel
    ├── Restart Button
    ├── Return to Menu Button
    └── Performance Toggle Button
```

### **Step 3: Setup Showcase Scene**

#### **Add Core Components**

```csharp
// Add to a GameObject in Showcase scene
var showcaseSetup = gameObject.AddComponent<ShowcaseSetup>();
var performanceMonitor = gameObject.AddComponent<PerformanceMonitor>();
```

#### **Configure ShowcaseSetup**

```
Prefab References:
- Fruit Prefabs: [Your fruit prefabs with Body components]
- Basket Prefab: [Your basket prefab with BasketBody]
- Hammer Prefab: [Your hammer prefab with HammerBody]
- Knife Prefab: [Your knife prefab with Knife component]

Spawn Settings:
- Spawn Area Size: (10, 5, 10)
- Spawn Area Center: (0, 8, 0)
- Spawn Delay: 0.1

Scene Objects:
- Fruit Parent: Empty GameObject for organizing fruits
- Tools Parent: Empty GameObject for organizing tools
- Main Camera: Reference to main camera
```

### **Step 4: Create Prefabs**

#### **Fruit Prefabs**

Create prefabs for each fruit type with:

- **Body component** (physics simulation)
- **SphereBody/MeshBody/CubeBody** (shape generation)
- **MeshRenderer** (visual representation)
- **Material** (appearance)

#### **Basket Prefab**

Create basket prefab with:

- **Body component**
- **BasketBody component**
- **BasketCollector component** (optional)
- **MeshRenderer** (visual)

#### **Hammer Prefab**

Create hammer prefab with:

- **Body component**
- **HammerBody component**
- **MeshRenderer** (visual)

#### **Knife Prefab**

Create knife prefab with:

- **Knife component**
- **MeshCollider** (for cutting)
- **MeshRenderer** (visual)

### **Step 5: Configure UI Elements**

#### **Main Menu UI**

```
Title Text: "Physics Simulation Showcase"
Start Button: Triggers scenario selection
Scenarios Button: Triggers scenario selection
Quit Button: Exits application
Version Text: "Physics Simulation v1.0"
```

#### **Parameter UI Elements**

```
Fruit Parameters:
- Fruit Count Slider: 10-1000 range
- Fruit Type Dropdown: All fruit types
- Fruit Count Text: Shows current value

Physics Parameters:
- Gravity Slider: -20 to 0 range
- Particle Optimization Toggle
- Adaptive Solver Toggle
- Solver Iterations Slider: 1-10 range
- Time Step Slider: 0.005-0.05 range
- Performance Mode Toggle

Tools Parameters:
- Basket Enabled Toggle
- Hammer Enabled Toggle
- Knife Enabled Toggle

Hammer Parameters (only visible when hammer enabled):
- Push Threshold Slider: 0.5-5 range
- Deform Threshold Slider: 2-10 range
- Crush Threshold Slider: 5-15 range
- Push Force Slider: 1-10 range
- Deform Force Slider: 5-20 range
- Crush Force Slider: 10-50 range
```

## 🎯 **Scenario Configurations**

### **Fruit Collection Paradise**

```
Fruit Settings:
- Fruit Count: 50
- Fruit Types: Apple, Orange, Banana, Strawberry
- Gravity: (0, -9.81, 0)

Physics Settings:
- Particle Optimization: true
- Adaptive Solver: true
- Solver Iterations: 5
- Time Step: 0.02
- Performance Mode: false

Tools:
- Basket Enabled: true
- Hammer Enabled: false
- Knife Enabled: false
```

### **Destruction Derby**

```
Fruit Settings:
- Fruit Count: 100
- Fruit Types: Watermelon, Pineapple, Coconut
- Gravity: (0, -12, 0)

Physics Settings:
- Particle Optimization: true
- Adaptive Solver: true
- Solver Iterations: 3
- Time Step: 0.016
- Performance Mode: false

Tools:
- Basket Enabled: false
- Hammer Enabled: true
- Knife Enabled: true

Hammer Settings:
- Push Threshold: 1.5 m/s
- Deform Threshold: 4.0 m/s
- Crush Threshold: 7.0 m/s
- Push Force: 5.0
- Deform Force: 12.0
- Crush Force: 25.0
```

### **Physics Sandbox**

```
Fruit Settings:
- Fruit Count: 500
- Fruit Types: Sphere, Cube, Complex
- Gravity: (0, -15, 0)

Physics Settings:
- Particle Optimization: true
- Adaptive Solver: true
- Solver Iterations: 2
- Time Step: 0.01
- Performance Mode: true

Tools:
- Basket Enabled: true
- Hammer Enabled: true
- Knife Enabled: true
```

## 🔧 **Advanced Configuration**

### **Custom Scenarios**

You can create custom scenarios by:

1. **Adding new scenarios** to the ShowcaseManager
2. **Configuring parameters** for your specific needs
3. **Creating custom prefabs** for unique objects
4. **Adjusting physics settings** for different gameplay styles

### **Performance Optimization**

For better performance:

1. **Reduce fruit count** for lower-end devices
2. **Enable performance mode** for large systems
3. **Reduce solver iterations** for faster simulation
4. **Use simpler fruit shapes** (spheres instead of complex meshes)

### **Visual Enhancement**

For better visuals:

1. **Add particle effects** for impacts
2. **Use post-processing** for better lighting
3. **Add audio effects** for interactions
4. **Use high-quality materials** for fruits

## 🎮 **User Experience**

### **Menu Flow**

1. **Main Menu** → User sees title and options
2. **Scenario Selection** → User chooses scenario
3. **Parameters** → User can adjust settings
4. **Showcase** → User experiences the simulation
5. **Controls** → User can restart or return to menu

### **Controls**

- **Mouse**: Move tools (basket, hammer, knife)
- **F3**: Toggle performance display
- **UI Buttons**: Navigate menus and control simulation

### **Performance Monitoring**

- **Real-time FPS** display
- **Particle count** tracking
- **Performance warnings** for low FPS
- **Automatic optimization** suggestions

## 🚨 **Troubleshooting**

### **Scene Loading Issues**

1. **Check scene names** match ShowcaseManager settings
2. **Verify prefabs** are assigned in ShowcaseSetup
3. **Ensure components** are properly attached
4. **Check build settings** include all scenes

### **Performance Issues**

1. **Reduce fruit count** for better performance
2. **Enable performance mode** for large systems
3. **Check particle optimization** is enabled
4. **Monitor FPS** and adjust settings accordingly

### **UI Issues**

1. **Verify UI references** are assigned in ShowcaseUI
2. **Check event listeners** are properly connected
3. **Ensure panels** are properly configured
4. **Test button functionality** in all menus

### **Physics Issues**

1. **Check gravity settings** are appropriate
2. **Verify solver iterations** are reasonable
3. **Ensure time step** is stable
4. **Test collision detection** is working

## 🎯 **Best Practices**

### **For Demonstrations**

1. **Start with Fruit Collection** for gentle introduction
2. **Move to Destruction Derby** for excitement
3. **End with Physics Sandbox** for technical showcase
4. **Adjust parameters** based on audience

### **For Development**

1. **Use performance mode** during testing
2. **Monitor particle counts** for optimization
3. **Test on target hardware** for realistic expectations
4. **Document custom scenarios** for future reference

### **For Deployment**

1. **Optimize prefabs** for target platform
2. **Test all scenarios** thoroughly
3. **Include performance monitoring** for user feedback
4. **Provide clear instructions** for users

---

_Your showcase system is now ready to demonstrate the full capabilities of your physics simulation project!_
