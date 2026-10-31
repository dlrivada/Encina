```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.488 μs | 0.5151 μs | 0.0282 μs |  1.00 |    2 | 0.0534 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.293 μs | 0.4445 μs | 0.0244 μs |  0.52 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.263 μs | 0.4243 μs | 0.0233 μs |  0.51 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.159 μs | 0.4286 μs | 0.0235 μs |  0.47 |    1 | 0.0210 | 0.0191 |     352 B |        0.37 |
