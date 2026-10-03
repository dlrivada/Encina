```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.392 μs | 0.0290 μs | 0.0192 μs |  1.15 |    0.03 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.301 μs | 0.0220 μs | 0.0145 μs |  1.08 |    0.02 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.210 μs | 0.0349 μs | 0.0231 μs |  1.00 |    0.03 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.169 μs | 0.0224 μs | 0.0148 μs |  0.97 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.232 μs | 0.0195 μs | 0.0116 μs |  1.02 |    0.02 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.338 μs | 0.3163 μs | 0.0173 μs |  1.10 |    0.01 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.313 μs | 0.1487 μs | 0.0082 μs |  1.08 |    0.01 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.212 μs | 0.0590 μs | 0.0032 μs |  1.00 |    0.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.156 μs | 0.0953 μs | 0.0052 μs |  0.95 |    0.00 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.236 μs | 0.5482 μs | 0.0300 μs |  1.02 |    0.02 | 0.0057 |     120 B |        1.00 |
