using UnityEngine;
using AnalyticsEvent = Unity.Services.Analytics.Event;

public class EventManager : MonoBehaviour
{
    public class LevelStartEvent : AnalyticsEvent
    {
        public LevelStartEvent() : base("LevelStart")
        {
        }

        public int level { set { SetParameter("level", value); } }
        public int round { set { SetParameter("round", value); } }
    }

    public class LevelCompleteEvent : AnalyticsEvent
    { 
        public LevelCompleteEvent() : base("LevelComplete")
        {
        }

        private int _level;
        private int _round;
        private int _time;

        public int level { get { return _level; } set { _level = value; SetParameter("level", value); } }
        public int round { get { return _round; } set { _round = value; SetParameter("round", value); } }
        public int time { get { return _time; } set { _time = value; SetParameter("time", value); } }
    }

    public class GameOverEvent : AnalyticsEvent
    {
        public GameOverEvent() : base("GameOver")
        {
        }

        public int level { set { SetParameter("level", value); } }
        public int time { set { SetParameter("time", value); } }
        public bool win { set { SetParameter("win", value); } }
        public string avatar { set { SetParameter("avatar", value); } }
        public string weapon { set { SetParameter("weapon", value); } }
        public string enemy { set { SetParameter("enemy", value); } }
    }

    public class ItemPickEvent : AnalyticsEvent
    {
        public ItemPickEvent() : base("ItemPick")
        {
        }
        public string itemName { set { SetParameter("itemName", value); } }
    }

    public class unLockEvent : AnalyticsEvent
    {
        public unLockEvent() : base("unLock")
        {
        }
        public string type { set { SetParameter("type", value); } }
        public string name { set { SetParameter("name", value); } }
        public int cost { set { SetParameter("cost", value); } }
    }
}




