namespace Siderectomy {
 // The existing paid probe completion is the sole authority for revealing the system.
 // This gate is shared by the map, sidebar and selection; it has no independent clock/state.
 public static class DemoSystemDiscovery {
  public static bool Revealed(DemoSimulation simulation){return simulation!=null&&simulation.GlobalProbe;}
  public static string UnknownView="当前查看：未知星系 / 上一级：当前关卡";
  public static string UnknownDetails="尚未探测星系\n天体数量、名称、轨道及关联关系：未知\n完成探测后揭示全貌；文明数据仍需深度侦察。";
 }
}
