```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 909.6 ns |  8.29 ns |  4.93 ns |  1.17 |    0.02 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 863.2 ns | 15.10 ns |  8.99 ns |  1.11 |    0.02 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 779.9 ns | 18.26 ns | 12.08 ns |  1.00 |    0.02 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 744.4 ns |  6.40 ns |  3.35 ns |  0.95 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 796.1 ns | 20.49 ns | 13.55 ns |  1.02 |    0.02 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |             |          |          |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 901.9 ns |  8.98 ns | 13.17 ns |  1.13 |    0.04 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 867.1 ns | 12.61 ns | 18.48 ns |  1.09 |    0.04 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 797.5 ns | 17.87 ns | 26.74 ns |  1.00 |    0.05 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 766.0 ns |  8.43 ns | 12.62 ns |  0.96 |    0.04 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 801.3 ns |  9.78 ns | 14.33 ns |  1.01 |    0.04 | 0.0067 |     120 B |        1.00 |
