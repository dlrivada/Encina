```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   2,782.7662 ns |    497.7093 ns |    27.2811 ns |  1.132 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        |  82,457.9410 ns | 49,604.6898 ns | 2,719.0004 ns | 33.546 |    1.00 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   2,437.8104 ns |    838.2451 ns |    45.9470 ns |  0.992 |    0.02 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   4,339.1772 ns |  5,545.8210 ns |   303.9852 ns |  1.765 |    0.11 |    3 |   1.3275 |   0.0076 |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.0959 ns |      0.5613 ns |     0.0308 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   2,146.6319 ns |    993.6481 ns |    54.4652 ns |  0.873 |    0.02 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 107,422.5336 ns | 47,482.0096 ns | 2,602.6492 ns | 43.702 |    0.98 |    5 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   4,359.4216 ns |    624.2447 ns |    34.2170 ns |  1.774 |    0.02 |    3 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   2,458.2218 ns |    425.8635 ns |    23.3430 ns |  1.000 |    0.01 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
