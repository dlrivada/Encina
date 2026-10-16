```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.527 μs | 0.0063 μs | 0.0042 μs |  1.15 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.519 μs | 0.0093 μs | 0.0062 μs |  1.14 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.329 μs | 0.0027 μs | 0.0018 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.283 μs | 0.0029 μs | 0.0015 μs |  0.97 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.347 μs | 0.0035 μs | 0.0018 μs |  1.01 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.454 μs | 0.0345 μs | 0.0019 μs |  1.04 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.423 μs | 0.0406 μs | 0.0022 μs |  1.02 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.400 μs | 0.2162 μs | 0.0118 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.290 μs | 0.0983 μs | 0.0054 μs |  0.92 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.340 μs | 0.0717 μs | 0.0039 μs |  0.96 | 0.0057 |     120 B |        1.00 |
