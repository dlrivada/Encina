```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     19.54 ns |     7.620 ns |   0.418 ns |   1.22 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     16.05 ns |     9.947 ns |   0.545 ns |   1.00 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 15,734.07 ns | 3,630.974 ns | 199.026 ns | 981.19 |   31.22 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     63.75 ns |    14.735 ns |   0.808 ns |   3.98 |    0.13 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     45.61 ns |    26.962 ns |   1.478 ns |   2.84 |    0.12 | 0.0105 |      - |     176 B |        1.57 |
