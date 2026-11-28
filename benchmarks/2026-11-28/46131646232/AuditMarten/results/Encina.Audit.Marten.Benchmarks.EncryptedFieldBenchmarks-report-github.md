```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  3,201.4645 ns |    540.6024 ns |  29.6322 ns |  1.331 |    0.01 |    2 |   0.0877 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 91,103.7680 ns | 11,984.7046 ns | 656.9221 ns | 37.876 |    0.24 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,670.0382 ns |    107.8181 ns |   5.9099 ns |  1.110 |    0.00 |    2 |   0.0191 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |  5,899.1434 ns |  2,724.2780 ns | 149.3269 ns |  2.453 |    0.05 |    3 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |      0.5205 ns |      0.2188 ns |   0.0120 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,299.7696 ns |    230.2051 ns |  12.6183 ns |  0.956 |    0.00 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 93,075.4153 ns |  8,278.4554 ns | 453.7701 ns | 38.696 |    0.16 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |  5,981.1809 ns |  4,919.3807 ns | 269.6479 ns |  2.487 |    0.10 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,405.2930 ns |     22.8895 ns |   1.2547 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
