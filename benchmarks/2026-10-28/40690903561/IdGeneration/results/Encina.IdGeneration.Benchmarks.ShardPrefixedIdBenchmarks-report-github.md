```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.522 μs | 0.0053 μs | 0.0035 μs |  1.17 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.483 μs | 0.0071 μs | 0.0047 μs |  1.14 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.301 μs | 0.0059 μs | 0.0031 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.256 μs | 0.0040 μs | 0.0024 μs |  0.97 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.328 μs | 0.0038 μs | 0.0025 μs |  1.02 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.460 μs | 0.0601 μs | 0.0033 μs |  1.11 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.424 μs | 0.1191 μs | 0.0065 μs |  1.09 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.310 μs | 0.1140 μs | 0.0062 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.350 μs | 0.0884 μs | 0.0048 μs |  1.03 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.333 μs | 0.0330 μs | 0.0018 μs |  1.02 | 0.0057 |     120 B |        1.00 |
