```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.342 μs | 1.4584 μs | 0.0799 μs |  1.00 |    0.04 |    2 | 0.0534 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.222 μs | 0.5677 μs | 0.0311 μs |  0.52 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.228 μs | 0.5659 μs | 0.0310 μs |  0.52 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.088 μs | 0.7623 μs | 0.0418 μs |  0.46 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        0.37 |
