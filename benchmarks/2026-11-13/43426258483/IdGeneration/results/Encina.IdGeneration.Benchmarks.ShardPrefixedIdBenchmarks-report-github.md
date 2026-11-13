```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,044.1 ns |   3.76 ns |  2.49 ns |  1.14 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,029.2 ns |   4.02 ns |  2.10 ns |  1.13 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     |   913.3 ns |   4.51 ns |  2.98 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   908.6 ns |   3.39 ns |  2.02 ns |  0.99 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     |   924.3 ns |   1.67 ns |  0.99 ns |  1.01 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |            |           |          |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,089.4 ns | 199.96 ns | 10.96 ns |  1.20 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,001.9 ns |  37.67 ns |  2.07 ns |  1.10 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           |   908.6 ns |  33.18 ns |  1.82 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   881.9 ns | 161.31 ns |  8.84 ns |  0.97 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           |   960.7 ns |  57.85 ns |  3.17 ns |  1.06 | 0.0067 |     120 B |        1.00 |
