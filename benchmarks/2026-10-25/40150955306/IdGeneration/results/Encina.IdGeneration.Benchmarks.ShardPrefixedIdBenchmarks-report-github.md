```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 932.9 ns | 24.81 ns | 14.76 ns |  1.12 |    0.02 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 916.2 ns | 17.03 ns | 11.27 ns |  1.10 |    0.02 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 829.6 ns | 19.44 ns | 12.86 ns |  1.00 |    0.02 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 789.6 ns | 19.61 ns | 12.97 ns |  0.95 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 826.3 ns | 25.17 ns | 14.98 ns |  1.00 |    0.02 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |             |          |          |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 910.4 ns | 15.44 ns | 22.14 ns |  1.10 |    0.04 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 907.1 ns | 11.23 ns | 16.81 ns |  1.10 |    0.04 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 828.8 ns | 16.68 ns | 24.45 ns |  1.00 |    0.04 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 781.4 ns | 11.31 ns | 16.57 ns |  0.94 |    0.03 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 855.3 ns | 11.85 ns | 17.37 ns |  1.03 |    0.04 | 0.0067 |     120 B |        1.00 |
