```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  2,959.0772 ns |   314.4421 ns |  17.2356 ns |  1.228 |    0.01 |    2 |   0.0877 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 90,440.5689 ns | 1,160.8640 ns |  63.6309 ns | 37.530 |    0.06 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,610.5664 ns |   105.0572 ns |   5.7585 ns |  1.083 |    0.00 |    2 |   0.0191 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |  5,399.1785 ns | 5,240.1470 ns | 287.2301 ns |  2.240 |    0.10 |    3 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |      0.5136 ns |     0.0299 ns |   0.0016 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,274.0927 ns |    83.0701 ns |   4.5534 ns |  0.944 |    0.00 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 93,723.5125 ns | 4,102.2246 ns | 224.8568 ns | 38.892 |    0.10 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |  5,856.7731 ns | 2,733.9645 ns | 149.8578 ns |  2.430 |    0.05 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,409.8541 ns |    71.1129 ns |   3.8979 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
