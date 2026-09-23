```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.74 ns |    13.176 ns |   0.722 ns |     1.28 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.64 ns |     0.877 ns |   0.048 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 50,683.60 ns | 6,154.103 ns | 337.327 ns | 2,342.23 |   14.23 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    100.39 ns |     2.776 ns |   0.152 ns |     4.64 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.20 ns |    10.288 ns |   0.564 ns |     3.01 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
