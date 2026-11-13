```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,838.3323 ns |     62.0408 ns |     3.4007 ns |  1.184 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 145,592.7457 ns | 20,914.2818 ns | 1,146.3824 ns | 35.642 |    0.34 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,529.0860 ns |    319.1603 ns |    17.4943 ns |  1.109 |    0.01 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   8,002.3989 ns |  5,774.0484 ns |   316.4951 ns |  1.959 |    0.07 |    3 |   1.3275 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.7058 ns |      0.3451 ns |     0.0189 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,832.3574 ns |    173.2173 ns |     9.4946 ns |  0.938 |    0.01 |    2 |   0.0229 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 168,113.7408 ns | 26,324.9250 ns | 1,442.9580 ns | 41.155 |    0.41 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   8,324.2759 ns |    589.1762 ns |    32.2947 ns |  2.038 |    0.02 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,085.0534 ns |    578.5660 ns |    31.7132 ns |  1.000 |    0.01 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
