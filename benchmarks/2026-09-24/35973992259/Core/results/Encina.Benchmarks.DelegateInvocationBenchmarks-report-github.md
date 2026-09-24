```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     18.85 ns |     5.958 ns |   0.327 ns |     1.28 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     14.69 ns |     2.676 ns |   0.147 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 15,692.91 ns | 3,341.627 ns | 183.166 ns | 1,068.37 |   14.18 | 0.3052 | 0.2899 |    5287 B |       47.21 |
| GenericTypeConstruction |     61.60 ns |    22.600 ns |   1.239 ns |     4.19 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     41.10 ns |     1.847 ns |   0.101 ns |     2.80 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
