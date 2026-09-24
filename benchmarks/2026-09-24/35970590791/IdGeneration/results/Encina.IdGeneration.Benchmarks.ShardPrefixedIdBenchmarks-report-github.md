```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.460 μs | 0.0071 μs | 0.0037 μs |  1.10 |    0.01 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.430 μs | 0.0087 μs | 0.0052 μs |  1.08 |    0.01 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.322 μs | 0.0175 μs | 0.0116 μs |  1.00 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.320 μs | 0.0054 μs | 0.0028 μs |  1.00 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.332 μs | 0.0064 μs | 0.0038 μs |  1.01 |    0.01 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.463 μs | 0.0585 μs | 0.0032 μs |  1.07 |    0.03 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.431 μs | 0.0652 μs | 0.0036 μs |  1.05 |    0.03 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.363 μs | 0.7324 μs | 0.0401 μs |  1.00 |    0.04 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.328 μs | 0.0970 μs | 0.0053 μs |  0.97 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.406 μs | 0.2545 μs | 0.0140 μs |  1.03 |    0.03 | 0.0057 |     120 B |        1.00 |
