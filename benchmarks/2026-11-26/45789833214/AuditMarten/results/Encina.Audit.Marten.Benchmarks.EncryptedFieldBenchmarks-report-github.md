```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   2,815.1933 ns |  1,648.6269 ns |    90.3668 ns |  1.169 |    0.04 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        |  81,122.2821 ns | 49,666.6564 ns | 2,722.3970 ns | 33.674 |    1.10 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   2,490.1786 ns |  1,559.4869 ns |    85.4807 ns |  1.034 |    0.03 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   4,366.2256 ns |  1,247.8560 ns |    68.3992 ns |  1.812 |    0.04 |    3 |   1.3275 |   0.0076 |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.2175 ns |      0.5331 ns |     0.0292 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   2,142.6867 ns |    166.3660 ns |     9.1191 ns |  0.889 |    0.01 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 106,891.8777 ns | 64,676.2314 ns | 3,545.1225 ns | 44.371 |    1.43 |    5 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   5,084.3398 ns |  4,190.2354 ns |   229.6809 ns |  2.111 |    0.09 |    3 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   2,409.4953 ns |    737.9218 ns |    40.4480 ns |  1.000 |    0.02 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
