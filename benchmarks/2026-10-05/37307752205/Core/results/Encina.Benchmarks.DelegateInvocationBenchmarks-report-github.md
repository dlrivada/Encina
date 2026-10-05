```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     19.45 ns |     3.901 ns |  0.214 ns |     1.30 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.01 ns |     2.773 ns |  0.152 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 15,914.46 ns | 1,537.569 ns | 84.279 ns | 1,060.59 |   10.53 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     61.81 ns |    19.975 ns |  1.095 ns |     4.12 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     42.79 ns |    17.137 ns |  0.939 ns |     2.85 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
