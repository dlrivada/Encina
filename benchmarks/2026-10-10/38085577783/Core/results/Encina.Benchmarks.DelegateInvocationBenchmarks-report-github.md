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
| CompiledDelegate        |     20.53 ns |    11.094 ns |   0.608 ns |     1.34 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.35 ns |     1.391 ns |   0.076 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 16,543.73 ns | 7,307.528 ns | 400.550 ns | 1,077.92 |   23.07 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     64.77 ns |    18.409 ns |   1.009 ns |     4.22 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     43.67 ns |     7.861 ns |   0.431 ns |     2.85 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
