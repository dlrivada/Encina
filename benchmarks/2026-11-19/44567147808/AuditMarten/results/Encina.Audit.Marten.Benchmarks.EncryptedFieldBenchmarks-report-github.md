```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,051.0595 ns |    721.6404 ns |    39.5555 ns |  1.173 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 154,439.3192 ns | 47,967.3436 ns | 2,629.2520 ns | 35.877 |    0.54 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,432.8928 ns |    142.2244 ns |     7.7958 ns |  1.030 |    0.00 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   8,560.1609 ns |    539.7106 ns |    29.5834 ns |  1.989 |    0.01 |    3 |   1.3275 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.6041 ns |      0.1331 ns |     0.0073 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,888.0249 ns |    250.7681 ns |    13.7454 ns |  0.903 |    0.00 |    2 |   0.0229 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 174,481.1085 ns | 17,048.3267 ns |   934.4763 ns | 40.533 |    0.22 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   9,486.4211 ns |  1,417.8915 ns |    77.7194 ns |  2.204 |    0.02 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,304.7435 ns |    268.2605 ns |    14.7043 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
