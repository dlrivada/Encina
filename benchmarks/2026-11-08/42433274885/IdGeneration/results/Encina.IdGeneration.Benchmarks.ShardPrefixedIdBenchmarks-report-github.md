```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 751.9 ns | 10.43 ns |  6.90 ns |  1.16 |    0.02 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 714.7 ns |  7.93 ns |  5.25 ns |  1.10 |    0.02 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 650.3 ns | 13.22 ns |  8.75 ns |  1.00 |    0.02 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 592.9 ns | 26.32 ns | 17.41 ns |  0.91 |    0.03 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 675.0 ns | 17.99 ns |  9.41 ns |  1.04 |    0.02 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |             |          |          |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 737.0 ns |  7.85 ns | 11.50 ns |  1.14 |    0.04 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 740.4 ns |  8.82 ns | 12.92 ns |  1.14 |    0.04 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 648.2 ns | 14.21 ns | 21.27 ns |  1.00 |    0.05 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 605.5 ns | 11.27 ns | 16.87 ns |  0.94 |    0.04 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 637.6 ns |  9.39 ns | 14.06 ns |  0.98 |    0.04 | 0.0067 |     120 B |        1.00 |
