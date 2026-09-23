```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,278.38 ns |     70.524 ns |     72.423 ns |     1,295.30 ns |     1.00 |     0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        55.70 ns |      0.089 ns |      0.102 ns |        55.66 ns |     0.04 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        22.50 ns |      0.085 ns |      0.087 ns |        22.46 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,414,511.46 ns | 12,529.082 ns | 14,428.509 ns | 5,416,144.68 ns | 4,248.18 |   232.83 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,550.40 ns | 13,207.207 ns |    723.932 ns |     1,159.05 ns |     1.13 |     0.60 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        55.35 ns |      0.876 ns |      0.048 ns |        55.36 ns |     0.04 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        22.34 ns |      0.197 ns |      0.011 ns |        22.34 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,416,105.26 ns | 97,602.982 ns |  5,349.949 ns | 5,416,405.53 ns | 3,946.13 | 1,260.66 | 7.8125 |      - |  209656 B |    1,455.94 |
