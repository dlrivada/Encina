```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.310 μs | 0.0035 μs | 0.0023 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.283 μs | 0.0016 μs | 0.0009 μs |  0.98 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.357 μs | 0.0078 μs | 0.0051 μs |  1.04 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.275 μs | 0.0116 μs | 0.0069 μs |  0.97 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.346 μs | 0.0070 μs | 0.0047 μs |  1.03 | 0.0114 |     216 B |        1.80 |
|                                |            |                |             |             |              |             |               |           |           |       |        |           |             |
| Generate_UlidFormat            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 23,169.705 μs |        NA | 0.0000 μs |  1.00 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,684.329 μs |        NA | 0.0000 μs |  0.89 |      - |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,820.368 μs |        NA | 0.0000 μs |  0.94 |      - |     152 B |        1.27 |
| ExtractShardId_Ulid            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 25,819.198 μs |        NA | 0.0000 μs |  1.11 |      - |     120 B |        1.00 |
| Generate_ToString              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 23,057.526 μs |        NA | 0.0000 μs |  1.00 |      - |     216 B |        1.80 |
