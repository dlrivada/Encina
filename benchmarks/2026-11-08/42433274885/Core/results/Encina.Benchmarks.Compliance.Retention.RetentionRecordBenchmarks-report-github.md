```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.193 μs | 0.0278 μs | 0.0416 μs |  1.00 |    0.02 |    2 | 0.0648 | 0.0305 |    1152 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.821 μs | 0.0080 μs | 0.0119 μs |  0.57 |    0.01 |    1 | 0.0362 | 0.0172 |     640 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.811 μs | 0.0078 μs | 0.0114 μs |  0.57 |    0.01 |    1 | 0.0362 | 0.0172 |     640 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.777 μs | 0.0079 μs | 0.0118 μs |  0.56 |    0.01 |    1 | 0.0362 | 0.0172 |     640 B |        0.56 |
