```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,685.9 ns | 165.43 ns | 242.49 ns |  1.02 |    0.20 |    3 | 0.0553 | 0.0267 |     976 B |        1.00 |
| &#39;Reject assessment&#39;  |   820.1 ns |  18.77 ns |  27.51 ns |  0.50 |    0.07 |    2 | 0.0219 | 0.0210 |     384 B |        0.39 |
| &#39;Request revision&#39;   |   813.1 ns |  23.46 ns |  35.11 ns |  0.49 |    0.07 |    2 | 0.0219 | 0.0210 |     384 B |        0.39 |
| &#39;Expire assessment&#39;  |   720.2 ns |  18.32 ns |  26.86 ns |  0.44 |    0.06 |    1 | 0.0210 | 0.0200 |     368 B |        0.38 |
