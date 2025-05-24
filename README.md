![Boxing Agent Screenshot](/docs/BoxingAgentOverview.png)

# Boxing Agent Unity Project

A reinforcement learning project using Unity ML-Agents Toolkit to train a humanoid agent capable of performing boxing attacks and blocks in a 3D environment.

## Agent

Based on the [Walker example](https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Examples.md) from ML-Agents Toolkit, this project extends the idea to a close-combat scenario.

The agent is trained using the PPO algorithm and performs offensive and defensive actions in response to the opponent's position and behavior.

![Agent Screenshot](/docs/BoxingAgentCloseup.png)

### Features:

* Custom boxing rig and animation controller created with Unity Animator.
* Discrete action space with attack/block/movement combinations.
* Reward shaping for hits, blocks, distance control, and staying upright.
* Configurable via YAML (training hyperparameters, curriculum, curiosity).

### Training Flow:

* Stage 1: BoxingAgent with `earlyTraining = true` — learns to stay upright and move.
* Stage 2: BoxingAgent with `earlyTraining = false` — learns to attack and block dynamically.
* Stage 3 (optional): Self-play mode — trains against a clone of itself or a previously saved policy.

### Files Included:

* `BoxingAgent.cs` – main agent logic
* `WalkerAgent.cs` – inherited movement logic
* `AttackHitbox.cs` – manages hit registration
* `AnimScript.cs` – synchronizes logic and animation states
* `Stabilizer.cs` – balance control
* `Walker.yaml` – training configuration
* Prefabs: Agent, opponent, target controller
* Example scenes: Flat arena, block obstacle arena

---

### Requirements:

* Unity 2022.x
* ML-Agents Toolkit (v2.3+)
* Python 3.8+
* PyTorch backend (via `mlagents-learn`)

---

### License

MIT. Free to use for educational or research purposes.
