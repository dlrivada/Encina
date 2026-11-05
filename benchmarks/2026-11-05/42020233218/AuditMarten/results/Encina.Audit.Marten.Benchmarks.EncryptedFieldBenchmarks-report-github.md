```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,009.3902 ns |    714.6079 ns |    39.1701 ns |  1.225 |    0.01 |    3 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 110,821.5583 ns |  5,259.3800 ns |   288.2844 ns | 33.866 |    0.11 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,316.7806 ns |     84.6457 ns |     4.6397 ns |  1.014 |    0.00 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   6,765.9493 ns |  1,970.6749 ns |   108.0193 ns |  2.068 |    0.03 |    4 |   1.3275 |   0.0076 |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.3720 ns |      2.7220 ns |     0.1492 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   2,846.7542 ns |     54.4114 ns |     2.9825 ns |  0.870 |    0.00 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 131,811.9398 ns | 23,522.6444 ns | 1,289.3555 ns | 40.281 |    0.35 |    5 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   7,006.1853 ns |  1,456.1949 ns |    79.8190 ns |  2.141 |    0.02 |    4 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,272.3242 ns |    153.9602 ns |     8.4391 ns |  1.000 |    0.00 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
