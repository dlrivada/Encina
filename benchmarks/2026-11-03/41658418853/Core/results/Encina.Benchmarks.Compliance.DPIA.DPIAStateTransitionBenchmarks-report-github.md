```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,369.2 ns | 1,233.5 ns | 67.61 ns |  1.00 |    0.06 |    2 | 0.0553 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  |   775.7 ns |   444.8 ns | 24.38 ns |  0.57 |    0.03 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Request revision&#39;   |   760.1 ns |   222.5 ns | 12.20 ns |  0.56 |    0.03 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   656.2 ns |   241.7 ns | 13.25 ns |  0.48 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        0.39 |
