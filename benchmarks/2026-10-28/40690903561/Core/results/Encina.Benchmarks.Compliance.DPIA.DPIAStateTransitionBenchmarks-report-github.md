```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.234 μs | 0.8794 μs | 0.0482 μs |  1.00 |    0.03 |    2 | 0.0534 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.238 μs | 0.2779 μs | 0.0152 μs |  0.55 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.199 μs | 0.2886 μs | 0.0158 μs |  0.54 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.041 μs | 0.4818 μs | 0.0264 μs |  0.47 |    0.01 |    1 | 0.0210 | 0.0191 |     352 B |        0.37 |
