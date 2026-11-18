```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   3,730.6206 ns |  1,666.2140 ns |    91.3308 ns |  1.075 |    0.03 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 108,360.6469 ns | 17,437.1437 ns |   955.7887 ns | 31.220 |    0.38 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,207.3130 ns |    461.4315 ns |    25.2926 ns |  0.924 |    0.01 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   5,766.0499 ns |  2,230.2807 ns |   122.2492 ns |  1.661 |    0.03 |    3 |   1.3275 |   0.0076 |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.2833 ns |      0.1579 ns |     0.0087 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   2,877.8720 ns |    777.4739 ns |    42.6160 ns |  0.829 |    0.01 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 129,842.0029 ns | 72,688.3497 ns | 3,984.2937 ns | 37.409 |    1.06 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   6,243.1409 ns |    829.5941 ns |    45.4729 ns |  1.799 |    0.02 |    3 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,471.1952 ns |    710.4744 ns |    38.9435 ns |  1.000 |    0.01 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
