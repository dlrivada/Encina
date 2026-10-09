```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.501 μs | 0.0051 μs | 0.0031 μs |  1.14 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.416 μs | 0.0018 μs | 0.0010 μs |  1.08 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.313 μs | 0.0027 μs | 0.0016 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.269 μs | 0.0036 μs | 0.0024 μs |  0.97 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.329 μs | 0.0056 μs | 0.0037 μs |  1.01 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.512 μs | 0.1080 μs | 0.0059 μs |  1.09 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.407 μs | 0.0376 μs | 0.0021 μs |  1.02 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.381 μs | 0.0862 μs | 0.0047 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.270 μs | 0.0951 μs | 0.0052 μs |  0.92 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.326 μs | 0.0435 μs | 0.0024 μs |  0.96 | 0.0057 |     120 B |        1.00 |
