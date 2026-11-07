```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 712.0 ns |  21.59 ns | 12.85 ns |  1.18 |    0.02 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 677.5 ns |  30.42 ns | 20.12 ns |  1.12 |    0.03 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 604.6 ns |  10.36 ns |  6.17 ns |  1.00 |    0.01 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 580.3 ns |  14.72 ns |  9.74 ns |  0.96 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 626.6 ns |   7.62 ns |  4.54 ns |  1.04 |    0.01 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |          |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 750.0 ns | 727.23 ns | 39.86 ns |  1.23 |    0.06 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 670.8 ns | 127.29 ns |  6.98 ns |  1.10 |    0.01 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 610.8 ns |  34.08 ns |  1.87 ns |  1.00 |    0.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 581.5 ns |  79.09 ns |  4.34 ns |  0.95 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 620.8 ns |  97.98 ns |  5.37 ns |  1.02 |    0.01 | 0.0067 |     120 B |        1.00 |
