```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,138.2 ns |  15.24 ns |  9.07 ns |  1.12 |    0.01 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,108.8 ns |   5.87 ns |  3.49 ns |  1.09 |    0.01 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1,018.8 ns |  16.87 ns | 10.04 ns |  1.00 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   970.2 ns |   4.76 ns |  2.49 ns |  0.95 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1,051.4 ns |  29.20 ns | 17.37 ns |  1.03 |    0.02 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |            |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,141.2 ns |  98.69 ns |  5.41 ns |  1.13 |    0.01 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,129.4 ns |  39.33 ns |  2.16 ns |  1.12 |    0.01 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1,009.8 ns | 106.31 ns |  5.83 ns |  1.00 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   973.7 ns | 104.15 ns |  5.71 ns |  0.96 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1,020.5 ns |  44.54 ns |  2.44 ns |  1.01 |    0.01 | 0.0057 |     120 B |        1.00 |
