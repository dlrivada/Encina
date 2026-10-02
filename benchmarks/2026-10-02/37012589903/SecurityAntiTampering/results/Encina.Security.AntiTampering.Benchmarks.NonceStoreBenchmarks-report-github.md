```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.39GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,266.49 ns |      62.204 ns |     61.092 ns |     1,285.16 ns |     1.00 |     0.07 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        55.75 ns |       0.025 ns |      0.024 ns |        55.75 ns |     0.04 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        22.27 ns |       0.010 ns |      0.010 ns |        22.27 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,409,398.72 ns |  14,415.974 ns | 16,023.317 ns | 5,412,170.06 ns | 4,280.84 |   208.08 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,569.59 ns |  13,533.385 ns |    741.811 ns |     1,172.84 ns |     1.13 |     0.61 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        55.52 ns |       0.533 ns |      0.029 ns |        55.54 ns |     0.04 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        22.28 ns |       0.522 ns |      0.029 ns |        22.27 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,439,712.74 ns | 224,630.282 ns | 12,312.744 ns | 5,446,179.44 ns | 3,926.39 | 1,267.73 | 7.8125 |      - |  209656 B |    1,455.94 |
