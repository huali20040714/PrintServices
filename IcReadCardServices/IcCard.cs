using Base;
using System;
using System.Text;

namespace IcReadCardServices
{
    public class IcCard
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        #region Ic卡相关接口

        /// <summary>
        /// 1、加载设备，设备初始化
        /// </summary>
        /// <returns></returns>
        public Result Init()
        {
            try
            {
                if (URF330.icdev <= 0)
                {
                    URF330.icdev = URF330.rf_init(0, 115200);
                    if (URF330.icdev > 0)
                        URF330.rf_beep(URF330.icdev, 5);
                }

                return Result.BuilderStream(URF330.icdev);
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
                return Result.BuilderStream(0, ex.Message);
            }
        }

        /// <summary>
        /// 2、读取IcCard机器码(唯一)
        /// </summary>
        public Result GetIcCardMachineId()
        {
            try
            {
                if (URF330.icdev <= 0)
                    URF330.icdev = URF330.rf_init(0, 115200);

                var ret = 0;
                var needChangePassword = false;             // 是否需要修改默认密码
                var sec = 0;                                // 第0扇区
                var block = 0;                              // 第0数据块
                var randomSecret = URF330.randomSecret;     // 服务默认读取密码
                var checkRet = Check(randomSecret, sec);
                if (checkRet != null)
                {
                    randomSecret = "ffffffffffff";          // 出厂默认密码尝试
                    checkRet = Check(randomSecret, sec);
                    if (checkRet != null)
                        return checkRet;

                    needChangePassword = true;
                }
                var machineId = ReadData(sec, block);
                if (machineId == string.Empty)
                    return Result.BuilderStream(0, "读取机器码失败");

                //修改成服务默认密码
                if (needChangePassword)
                {
                    ret = ChangePassword(sec, URF330.randomSecret);
                    if (ret != 0)
                        return Result.BuilderStream(0, "读取机器码修改读取密码失败");
                }

                URF330.rf_beep(URF330.icdev, 5);
                return Result.BuilderStream(URF330.icdev, string.Empty, machineId);
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
                return Result.BuilderStream(0, ex.Message);
            }
        }

        /// <summary>
        /// 3、读取IcCard信息
        /// </summary>
        public Result GetIcCard(string randomSecret)
        {
            try
            {
                var sec = 1;                                // 第1扇区
                var block = 0;                              // 第0数据块
                var checkRet = Check(randomSecret, sec);
                if (checkRet != null)
                    return checkRet;

                var dataStr = ReadData(sec, block);
                if (dataStr == string.Empty)
                    return Result.BuilderStream(0, "读取数据失败");

                return Result.BuilderStream(URF330.icdev, string.Empty, string.Empty, dataStr);
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
                return Result.BuilderStream(0, ex.Message);
            }
        }

        /// <summary>
        /// 4、写入IcCard数据
        /// </summary>        
        public Result WriteIcCard(string randomSecret, string data1)
        {
            try
            {
                var sec = 1;                                // 第1扇区
                var block = 0;                              // 第0数据块
                var checkRet = Check(randomSecret, sec);
                if (checkRet != null)
                    return checkRet;

                if (!string.IsNullOrEmpty(data1) && data1.Length != 32)
                    return Result.BuilderStream(0, "写入数据长度不对");

                if (data1.Length > 0)
                {
                    //3、写入第一扇区
                    var buff = Encoding.ASCII.GetBytes(data1);
                    byte[] databuff = new byte[16];
                    URF330.a_hex(buff, databuff, 32);
                    var ret = MifareOne.rf_write(URF330.icdev, sec * 4 + block, databuff);
                    if (ret != 0)
                        return Result.BuilderStream(0, "写入数据失败");

                    return Result.BuilderStream(URF330.icdev);
                }
                else
                {
                    return Result.BuilderStream(0, "写入数据不能为空");
                }
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
                return Result.BuilderStream(0, ex.Message);
            }
        }

        /// <summary>
        /// 5、修改KeyA
        /// </summary>
        public Result ChangeKeyIcCard(string randomSecret, string newRandomSecret)
        {
            try
            {
                var ret = 0;
                var sec = 1;                                // 第1扇区
                var checkRet = Check(randomSecret, sec);
                if (checkRet != null)
                    return checkRet;

                ret = ChangePassword(sec, newRandomSecret);
                if (ret != 0)
                    return Result.BuilderStream(0, "修改密码失败");

                return Result.BuilderStream(URF330.icdev, "密码修改成功");
            }
            catch (Exception ex)
            {
                this.Logger?.Log(ex.Message);
                return Result.BuilderStream(0, ex.Message);
            }
        }

        #endregion

        #region Ic卡(私有方法)

        //寻址，获取机器码(唯一标识)
        private Result Auticall(out uint machineId)
        {
            machineId = 0;
            if (URF330.icdev <= 0)
                return Result.BuilderStream(URF330.icdev, "初始化失败");

            var ret = MifareOne.rf_reset(URF330.icdev, 3);
            if (ret != 0)
                return Result.BuilderStream(0, "寻卡失败[rf_reset]");

            ret = MifareOne.rf_request(URF330.icdev, 1, out UInt16 tagtype);
            if (ret != 0)
                return Result.BuilderStream(0, "寻卡失败[request]");

            ret = MifareOne.rf_anticoll(URF330.icdev, 0, out machineId);
            if (ret != 0)
                return Result.BuilderStream(0, "寻卡失败[anticoll]");

            return null;
        }

        //验证：寻址、加载密码，验证密码（返回 null 表示成功）
        private Result Check(string randomSecret, int sec)
        {
            if (URF330.icdev <= 0)
                return Result.BuilderStream(URF330.icdev, "未初始化成功");

            //1、寻卡
            var ret = MifareOne.rf_reset(URF330.icdev, 3);
            if (ret != 0)
            {
                URF330.icdev = 0;
                return Result.BuilderStream(0, "重置失败[rf_reset]");
            }

            ret = MifareOne.rf_request(URF330.icdev, 1, out UInt16 tagtype);
            if (ret != 0)
                return Result.BuilderStream(0, "寻卡失败[request]");

            ret = MifareOne.rf_anticoll(URF330.icdev, 0, out uint machineId);
            if (ret != 0)
                return Result.BuilderStream(0, "寻卡失败[anticoll]");

            ret = MifareOne.rf_select(URF330.icdev, machineId, out byte size);
            if (ret != 0)
                return Result.BuilderStream(0, "寻卡失败[select]");

            //2、认证
            randomSecret = randomSecret ?? "";
            var validLen = 0;
            for (int i = 0; i < randomSecret.Length; i++)
            {
                if ((randomSecret[i] >= '0' && randomSecret[i] <= '9') || (randomSecret[i] >= 'a' && randomSecret[i] <= 'f') || (randomSecret[i] >= 'A' && randomSecret[i] <= 'F'))
                    validLen++;
            }
            if (validLen != randomSecret.Length)
                return Result.BuilderStream(0, "认证失败[密码必须为十六进制数]");

            var key1 = Encoding.ASCII.GetBytes(randomSecret);
            byte[] key2 = new byte[7];
            URF330.a_hex(key1, key2, 12);
            ret = URF330.rf_load_key(URF330.icdev, 0, sec, key2);
            if (ret != 0)
                return Result.BuilderStream(0, "认证失败[装载密码失败]");

            ret = MifareOne.rf_authentication(URF330.icdev, 0, sec);
            if (ret != 0)
                return Result.BuilderStream(0, "认证失败");

            return null;
        }

        //修改密码，KeyA 和 KeyB
        private int ChangePassword(int sec, string password)
        {
            byte[] keyA1 = new byte[17];
            byte[] keyA2 = new byte[7];
            byte[] keyB1 = new byte[17];
            byte[] keyB2 = new byte[7];

            keyA1 = Encoding.ASCII.GetBytes(password);
            URF330.a_hex(keyA1, keyA2, 12);
            keyB1 = Encoding.ASCII.GetBytes(password);
            URF330.a_hex(keyB1, keyB2, 12);

            return MifareOne.rf_changeb3(URF330.icdev, sec, keyA2, 0x00, 0x00, 0x00, 0x01, 105, keyB2);
        }

        //读取数据（sec扇区[最大15]，block数据块[最大3], 参数都是从0开始）
        private string ReadData(int sec, int block)
        {
            byte[] dataBytes = new byte[16];
            byte[] buff = new byte[32];
            for (int i = 0; i < 16; i++)
                dataBytes[i] = 0;

            for (int i = 0; i < 32; i++)
                buff[i] = 0;

            var ret = MifareOne.rf_read(URF330.icdev, sec * 4 + block, dataBytes);
            if (ret != 0)
                return string.Empty;

            URF330.hex_a(dataBytes, buff, 16);
            return Encoding.ASCII.GetString(buff);
        }

        #endregion
    }
}
