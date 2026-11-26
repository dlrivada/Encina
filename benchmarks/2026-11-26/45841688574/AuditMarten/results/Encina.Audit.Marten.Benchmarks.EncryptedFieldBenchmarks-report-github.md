```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,360.7382 ns |    241.7370 ns |  13.2504 ns |  1.170 |    0.01 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 116,495.9287 ns | 17,534.8690 ns | 961.1453 ns | 31.246 |    0.36 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,820.1479 ns |    123.1358 ns |   6.7495 ns |  1.025 |    0.01 |    2 |   0.0191 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   7,909.8951 ns |    487.1104 ns |  26.7002 ns |  2.122 |    0.02 |    3 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.2906 ns |      0.8370 ns |   0.0459 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,486.5640 ns |    302.0588 ns |  16.5569 ns |  0.935 |    0.01 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 121,107.7063 ns | 13,192.3617 ns | 723.1179 ns | 32.483 |    0.34 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   8,875.2145 ns |    613.1225 ns |  33.6073 ns |  2.380 |    0.02 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,728.6212 ns |    718.6368 ns |  39.3909 ns |  1.000 |    0.01 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
