-- LuaBehaviour可以自行require脚本，但按当前UI框架的设计，UI的一些属性配置在了UI类的静
-- 态变量中，所以这里先require脚本，拿到UI类，再进行UI的加载操作。
require("UI.UIWelcome");
CSLike.UIManager.Instance:GotoScene(CSLike.UIWelcome);