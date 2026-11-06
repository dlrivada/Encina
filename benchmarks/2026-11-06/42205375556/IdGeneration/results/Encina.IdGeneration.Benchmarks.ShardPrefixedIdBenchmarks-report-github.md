```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 903.8 ns |  22.96 ns | 15.19 ns |  1.17 |    0.02 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 864.2 ns |  26.87 ns | 17.77 ns |  1.12 |    0.03 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 772.5 ns |  14.14 ns |  9.35 ns |  1.00 |    0.02 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 746.0 ns |  10.27 ns |  6.11 ns |  0.97 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 953.3 ns |  12.16 ns |  7.24 ns |  1.23 |    0.02 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |          |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 894.1 ns | 191.45 ns | 10.49 ns |  1.15 |    0.01 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 865.3 ns |  64.37 ns |  3.53 ns |  1.11 |    0.01 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 777.4 ns |  86.64 ns |  4.75 ns |  1.00 |    0.01 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 746.9 ns |  79.90 ns |  4.38 ns |  0.96 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 810.5 ns |  94.16 ns |  5.16 ns |  1.04 |    0.01 | 0.0067 |     120 B |        1.00 |
