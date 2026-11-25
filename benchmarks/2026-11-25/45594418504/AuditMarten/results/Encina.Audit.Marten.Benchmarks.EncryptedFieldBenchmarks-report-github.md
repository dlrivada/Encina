```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.93GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,949.9118 ns |  1,382.0289 ns |    75.7537 ns |  1.060 |    0.01 |    3 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 148,498.5387 ns | 18,184.8223 ns |   996.7715 ns | 31.806 |    0.21 |    5 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,668.6383 ns |    471.4715 ns |    25.8429 ns |  1.000 |    0.01 |    3 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   8,327.0119 ns |  3,784.1173 ns |   207.4202 ns |  1.784 |    0.04 |    4 |   1.3275 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.6047 ns |      0.0466 ns |     0.0026 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,853.1042 ns |    989.8176 ns |    54.2552 ns |  0.825 |    0.01 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 172,399.4271 ns | 30,030.0677 ns | 1,646.0493 ns | 36.925 |    0.33 |    5 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   8,924.5592 ns |  3,481.9291 ns |   190.8563 ns |  1.911 |    0.04 |    4 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,668.9504 ns |    316.8260 ns |    17.3663 ns |  1.000 |    0.00 |    3 |   0.0839 |        - |        - |    1496 B |        1.00 |
