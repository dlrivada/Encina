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
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.240 μs | 0.0067 μs | 0.0040 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.283 μs | 0.0072 μs | 0.0043 μs |  1.04 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.351 μs | 0.0027 μs | 0.0016 μs |  1.09 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.338 μs | 0.0111 μs | 0.0066 μs |  1.08 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.344 μs | 0.0079 μs | 0.0053 μs |  1.08 | 0.0114 |     216 B |        1.80 |
|                                |            |                |             |             |              |             |               |           |           |       |        |           |             |
| Generate_UlidFormat            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 22,559.972 μs |        NA | 0.0000 μs |  1.00 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,510.732 μs |        NA | 0.0000 μs |  0.91 |      - |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,885.561 μs |        NA | 0.0000 μs |  0.97 |      - |     152 B |        1.27 |
| ExtractShardId_Ulid            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 25,923.758 μs |        NA | 0.0000 μs |  1.15 |      - |     120 B |        1.00 |
| Generate_ToString              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 23,255.972 μs |        NA | 0.0000 μs |  1.03 |      - |     216 B |        1.80 |
