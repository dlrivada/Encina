```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,019.1 ns |  11.01 ns |  6.55 ns |  0.94 |    0.01 |      - |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,132.6 ns |  21.85 ns | 13.00 ns |  1.04 |    0.01 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1,088.6 ns |  14.71 ns |  7.70 ns |  1.00 |    0.01 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   935.9 ns |  26.05 ns | 17.23 ns |  0.86 |    0.02 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1,120.2 ns |  25.53 ns | 16.89 ns |  1.03 |    0.02 |      - |     120 B |        1.00 |
|                                |            |                |             |            |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,008.5 ns | 161.09 ns |  8.83 ns |  0.93 |    0.01 |      - |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,122.7 ns | 181.01 ns |  9.92 ns |  1.04 |    0.01 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1,081.3 ns | 252.84 ns | 13.86 ns |  1.00 |    0.02 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   920.6 ns | 164.87 ns |  9.04 ns |  0.85 |    0.01 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1,083.9 ns | 279.20 ns | 15.30 ns |  1.00 |    0.02 |      - |     120 B |        1.00 |
