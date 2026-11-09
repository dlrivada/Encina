```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,022.0805 ns |  2,304.5968 ns |   126.3227 ns |  1.147 |    0.05 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 149,164.9819 ns | 24,895.1577 ns | 1,364.5876 ns | 34.057 |    1.40 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,297.6249 ns |    447.6274 ns |    24.5360 ns |  0.981 |    0.04 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   8,111.2938 ns |  1,785.4156 ns |    97.8647 ns |  1.852 |    0.08 |    3 |   1.3275 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.7991 ns |      3.9667 ns |     0.2174 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,659.3403 ns |     63.3942 ns |     3.4748 ns |  0.835 |    0.03 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 175,506.7951 ns | 20,394.8028 ns | 1,117.9080 ns | 40.071 |    1.63 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   9,093.3414 ns |    459.0596 ns |    25.1626 ns |  2.076 |    0.08 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,386.0043 ns |  3,623.4991 ns |   198.6162 ns |  1.001 |    0.06 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
