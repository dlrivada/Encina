```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 1.467 μs | 0.0065 μs | 0.0039 μs | 1.466 μs |  1.10 |    0.00 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 1.426 μs | 0.0040 μs | 0.0024 μs | 1.427 μs |  1.07 |    0.00 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 1.336 μs | 0.0087 μs | 0.0052 μs | 1.335 μs |  1.00 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 1.282 μs | 0.0120 μs | 0.0079 μs | 1.280 μs |  0.96 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 1.351 μs | 0.0036 μs | 0.0019 μs | 1.351 μs |  1.01 |    0.00 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 1.445 μs | 0.0051 μs | 0.0068 μs | 1.444 μs |  1.03 |    0.04 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 1.449 μs | 0.0036 μs | 0.0051 μs | 1.451 μs |  1.03 |    0.04 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 1.404 μs | 0.0421 μs | 0.0617 μs | 1.453 μs |  1.00 |    0.06 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 1.333 μs | 0.0180 μs | 0.0264 μs | 1.349 μs |  0.95 |    0.05 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 1.384 μs | 0.0226 μs | 0.0317 μs | 1.361 μs |  0.99 |    0.05 | 0.0057 |     120 B |        1.00 |
