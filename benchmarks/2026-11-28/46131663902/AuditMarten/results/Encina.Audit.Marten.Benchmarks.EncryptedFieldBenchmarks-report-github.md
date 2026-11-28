```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  3,240.4841 ns |    183.5756 ns |    10.0624 ns |  1.331 |    0.00 |    3 |   0.0877 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 88,962.6698 ns |  1,367.6579 ns |    74.9659 ns | 36.544 |    0.09 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,698.2197 ns |    505.8446 ns |    27.7271 ns |  1.108 |    0.01 |    2 |   0.0191 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |  6,194.2761 ns |    519.1262 ns |    28.4551 ns |  2.544 |    0.01 |    4 |   0.2594 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |      0.5008 ns |      0.3037 ns |     0.0166 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,323.9611 ns |    123.7540 ns |     6.7834 ns |  0.955 |    0.00 |    2 |   0.0038 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 94,360.7140 ns | 31,966.5347 ns | 1,752.1936 ns | 38.761 |    0.63 |    6 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |  7,435.8158 ns |    890.1265 ns |    48.7908 ns |  3.054 |    0.02 |    5 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,434.4324 ns |    115.2323 ns |     6.3163 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
