```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,776.248 ns |    18.6381 ns |    27.8967 ns |  1.177 |    0.02 |    4 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 149,076.084 ns |   997.1394 ns | 1,461.5941 ns | 30.367 |    0.53 |    8 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,821.115 ns |    72.1795 ns |   103.5177 ns |  0.982 |    0.03 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,910.336 ns |    38.9104 ns |    58.2393 ns |  1.815 |    0.03 |    5 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.228 ns |     0.0026 ns |     0.0038 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,140.966 ns |     5.6920 ns |     7.7912 ns |  0.844 |    0.01 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 131,944.469 ns | 1,227.0238 ns | 1,836.5515 ns | 26.877 |    0.54 |    7 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,934.034 ns |   145.4721 ns |   217.7358 ns |  2.227 |    0.05 |    6 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,910.226 ns |    53.0888 ns |    72.6686 ns |  1.000 |    0.02 |    3 |   0.0839 |        - |        - |    1496 B |        1.00 |
