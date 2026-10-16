```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2,043.4 ns | 1,234.5 ns | 67.67 ns |  1.00 |    0.04 |    2 | 0.0725 | 0.0343 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   738.6 ns |   885.5 ns | 48.54 ns |  0.36 |    0.02 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,744.8 ns |   169.7 ns |  9.30 ns |  0.85 |    0.03 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,481.4 ns |   587.7 ns | 32.21 ns |  0.73 |    0.03 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Record: track entity&#39;                         | 2,519.5 ns |   676.3 ns | 37.07 ns |  1.23 |    0.04 |    3 | 0.0648 | 0.0305 |    1088 B |        0.87 |
| &#39;Record: mark expired&#39;                         | 1,520.1 ns | 1,118.2 ns | 61.29 ns |  0.74 |    0.03 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark deleted (terminal)&#39;              | 1,487.3 ns |   697.6 ns | 38.24 ns |  0.73 |    0.03 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Record: mark anonymized (terminal)&#39;           | 1,508.5 ns |   319.9 ns | 17.53 ns |  0.74 |    0.02 |    2 | 0.0362 | 0.0172 |     608 B |        0.49 |
| &#39;Legal hold: place (cross-aggregate)&#39;          | 1,664.9 ns |   676.9 ns | 37.10 ns |  0.82 |    0.03 |    2 | 0.0572 | 0.0286 |     960 B |        0.77 |
| &#39;Legal hold: lift&#39;                             | 1,459.9 ns |   186.1 ns | 10.20 ns |  0.71 |    0.02 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
| &#39;Legal hold: has active holds (read-only)&#39;     |   674.8 ns |   745.0 ns | 40.84 ns |  0.33 |    0.02 |    1 | 0.0343 | 0.0172 |     592 B |        0.47 |
