```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  2,825.1584 ns |  1,175.6218 ns |  64.4398 ns |  1.196 |    0.02 |    2 |   0.0877 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 89,298.1169 ns | 15,470.0929 ns | 847.9680 ns | 37.812 |    0.35 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,601.1647 ns |    424.8634 ns |  23.2882 ns |  1.101 |    0.01 |    2 |   0.0191 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |  4,983.4465 ns |    657.6120 ns |  36.0459 ns |  2.110 |    0.02 |    3 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |      0.5014 ns |      0.0295 ns |   0.0016 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,309.2777 ns |    102.3544 ns |   5.6104 ns |  0.978 |    0.00 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 92,854.5519 ns |  2,534.5816 ns | 138.9290 ns | 39.318 |    0.18 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |  5,408.0922 ns |    313.2917 ns |  17.1726 ns |  2.290 |    0.01 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,361.6739 ns |    221.0491 ns |  12.1164 ns |  1.000 |    0.01 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
