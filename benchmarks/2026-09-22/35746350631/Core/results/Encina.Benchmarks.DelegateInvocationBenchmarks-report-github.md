```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     20.69 ns |     7.450 ns |   0.408 ns |   1.22 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     17.03 ns |     3.210 ns |   0.176 ns |   1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,011.64 ns | 4,960.611 ns | 271.908 ns | 999.25 |   16.50 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     70.92 ns |    11.121 ns |   0.610 ns |   4.17 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     45.54 ns |     3.984 ns |   0.218 ns |   2.68 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
