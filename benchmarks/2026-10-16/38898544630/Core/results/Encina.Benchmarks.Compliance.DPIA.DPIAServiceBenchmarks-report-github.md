```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Create assessment (fast path)&#39;                      | 1,503.6 ns |   736.38 ns | 40.36 ns |  1.00 |    0.03 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Evaluate assessment (risk engine)&#39;                  | 1,380.2 ns |   208.16 ns | 11.41 ns |  0.92 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;RequiresDPIA check (pipeline hot-path)&#39;             |   976.6 ns |   135.25 ns |  7.41 ns |  0.65 |    0.02 |    1 | 0.0191 | 0.0181 |     336 B |        0.78 |
| &#39;Request DPO consultation&#39;                           | 1,294.9 ns |   150.86 ns |  8.27 ns |  0.86 |    0.02 |    1 | 0.0248 | 0.0229 |     432 B |        1.00 |
| &#39;Approve assessment&#39;                                 | 2,415.8 ns | 1,001.12 ns | 54.87 ns |  1.61 |    0.05 |    2 | 0.0534 | 0.0267 |     944 B |        2.19 |
| &#39;Reject assessment&#39;                                  | 1,267.4 ns |   241.77 ns | 13.25 ns |  0.84 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Request revision&#39;                                   | 1,255.2 ns |   219.13 ns | 12.01 ns |  0.84 |    0.02 |    1 | 0.0210 | 0.0191 |     368 B |        0.85 |
| &#39;Expire assessment&#39;                                  | 1,100.6 ns |   383.00 ns | 20.99 ns |  0.73 |    0.02 |    1 | 0.0210 | 0.0191 |     352 B |        0.81 |
| &#39;Get assessment by ID (cached)&#39;                      | 1,136.9 ns |   650.92 ns | 35.68 ns |  0.76 |    0.03 |    1 | 0.0248 | 0.0229 |     424 B |        0.98 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   987.9 ns |    56.63 ns |  3.10 ns |  0.66 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        0.91 |
| &#39;Get expired assessments&#39;                            |   944.8 ns |   268.50 ns | 14.72 ns |  0.63 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
| &#39;Get all assessments&#39;                                |   914.5 ns |   124.48 ns |  6.82 ns |  0.61 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.93 |
