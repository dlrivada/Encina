```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  3,128.6357 ns |   434.7546 ns |  23.8304 ns |  1.295 |    0.01 |    2 |   0.0877 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 91,392.5776 ns | 9,637.4878 ns | 528.2632 ns | 37.834 |    0.19 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,696.9586 ns |    21.3977 ns |   1.1729 ns |  1.116 |    0.00 |    2 |   0.0191 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |  5,924.5781 ns | 2,849.8014 ns | 156.2072 ns |  2.453 |    0.06 |    3 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |      0.5216 ns |     0.0373 ns |   0.0020 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,284.7269 ns |   120.2215 ns |   6.5897 ns |  0.946 |    0.00 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 92,368.5374 ns | 8,970.6531 ns | 491.7118 ns | 38.238 |    0.18 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |  6,748.2074 ns | 5,814.5179 ns | 318.7133 ns |  2.794 |    0.11 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,415.6227 ns |     8.9900 ns |   0.4928 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
