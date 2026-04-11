```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error       | StdDev        | Median         | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|------------:|--------------:|---------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,534.851 ns |  36.9818 ns |    53.0382 ns |   5,515.300 ns |  1.135 |    0.01 |    5 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 144,098.571 ns | 660.7103 ns |   904.3877 ns | 143,979.623 ns | 29.549 |    0.27 |    9 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,664.366 ns |  17.3387 ns |    25.4148 ns |   4,667.197 ns |  0.956 |    0.01 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,137.674 ns |  80.0873 ns |   119.8709 ns |   8,149.816 ns |  1.669 |    0.03 |    6 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.226 ns |   0.0023 ns |     0.0032 ns |       1.226 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,271.571 ns |  75.3566 ns |   105.6394 ns |   4,183.043 ns |  0.876 |    0.02 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 124,821.813 ns | 809.0064 ns | 1,210.8828 ns | 124,890.248 ns | 25.596 |    0.30 |    8 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,744.266 ns | 103.7992 ns |   148.8657 ns |   9,692.990 ns |  1.998 |    0.03 |    7 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,876.795 ns |  23.5035 ns |    33.7081 ns |   4,887.468 ns |  1.000 |    0.01 |    4 |   0.0839 |        - |        - |    1496 B |        1.00 |
