```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean          | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |--------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  4,290.874 ns |    723.4842 ns |    39.6566 ns |  1.157 |    0.01 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 97,990.929 ns | 29,112.4349 ns | 1,595.7508 ns | 26.422 |    0.37 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  3,755.681 ns |  1,599.9928 ns |    87.7010 ns |  1.013 |    0.02 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |  6,254.322 ns |  3,060.5007 ns |   167.7564 ns |  1.686 |    0.04 |    3 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |      1.031 ns |      0.2220 ns |     0.0122 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  3,372.775 ns |     71.9472 ns |     3.9437 ns |  0.909 |    0.00 |    2 |   0.0038 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 77,891.631 ns |  4,916.4398 ns |   269.4867 ns | 21.003 |    0.07 |    4 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  6,839.038 ns |  2,257.4964 ns |   123.7410 ns |  1.844 |    0.03 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  3,708.676 ns |     80.2855 ns |     4.4007 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
