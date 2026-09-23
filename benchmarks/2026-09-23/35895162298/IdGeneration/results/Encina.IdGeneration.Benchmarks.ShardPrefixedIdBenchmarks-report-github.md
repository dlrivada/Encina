```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.131 μs | 0.0051 μs | 0.0034 μs |  0.93 |    0.00 |      - |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.258 μs | 0.0126 μs | 0.0066 μs |  1.04 |    0.01 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.212 μs | 0.0075 μs | 0.0050 μs |  1.00 |    0.01 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.044 μs | 0.0143 μs | 0.0094 μs |  0.86 |    0.01 |      - |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.207 μs | 0.0123 μs | 0.0081 μs |  1.00 |    0.01 |      - |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.121 μs | 0.0638 μs | 0.0035 μs |  0.91 |    0.01 |      - |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.260 μs | 0.1872 μs | 0.0103 μs |  1.03 |    0.01 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.228 μs | 0.3033 μs | 0.0166 μs |  1.00 |    0.02 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.049 μs | 0.0961 μs | 0.0053 μs |  0.85 |    0.01 |      - |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.222 μs | 0.2687 μs | 0.0147 μs |  0.99 |    0.02 |      - |     120 B |        1.00 |
