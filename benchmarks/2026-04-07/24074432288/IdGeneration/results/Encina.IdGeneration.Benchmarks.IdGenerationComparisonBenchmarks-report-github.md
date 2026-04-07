```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           |   241.7 ns |  0.10 ns |  0.07 ns |   241.7 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           |   959.8 ns |  6.89 ns |  4.10 ns |   958.6 ns |  3.97 |    0.02 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           |   963.6 ns |  1.54 ns |  1.02 ns |   963.5 ns |  3.99 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 1,158.9 ns |  3.29 ns |  1.72 ns | 1,159.0 ns |  4.80 |    0.01 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           |   395.9 ns |  0.37 ns |  0.22 ns |   396.0 ns |  1.64 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          |   241.6 ns |  0.05 ns |  0.07 ns |   241.7 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          |   954.5 ns | 11.60 ns | 17.00 ns |   968.5 ns |  3.95 |    0.07 |    3 |      - |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          |   964.4 ns |  0.98 ns |  1.34 ns |   964.2 ns |  3.99 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 1,190.6 ns | 22.28 ns | 32.66 ns | 1,163.3 ns |  4.93 |    0.13 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          |   397.3 ns |  1.33 ns |  1.86 ns |   398.5 ns |  1.64 |    0.01 |    2 |      - |         - |          NA |
